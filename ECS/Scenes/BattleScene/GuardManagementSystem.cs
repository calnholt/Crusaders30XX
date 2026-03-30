using System;
using System.Linq;
using System.Collections.Generic;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Events;
using Microsoft.Xna.Framework;

namespace Crusaders30XX.ECS.Systems
{
	public class GuardManagementSystem : Core.System
	{
		public static readonly float Duration = 0.5f;

		public GuardManagementSystem(EntityManager entityManager) : base(entityManager)
		{
			EventManager.Subscribe<AddGuardEvent>(OnAddGuard);
			EventManager.Subscribe<ChangeBattlePhaseEvent>(OnChangeBattlePhase);
			EventManager.Subscribe<LoadSceneEvent>(OnLoadScene);
		}

		protected override IEnumerable<Entity> GetRelevantEntities()
		{
			return EntityManager.GetEntitiesWithComponent<GuardQueue>();
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime) { }

		private void OnAddGuard(AddGuardEvent e)
		{
			if (e.Enemy == null || e.Value <= 0) return;
			var gq = e.Enemy.GetComponent<GuardQueue>();
			if (gq == null)
			{
				gq = new GuardQueue();
				EntityManager.AddComponent(e.Enemy, gq);
			}
			gq.Queue.Add(e.Value);
			EventManager.Publish(new GuardGainedEvent { Enemy = e.Enemy, GuardValue = e.Value });
		}

		private void OnChangeBattlePhase(ChangeBattlePhaseEvent evt)
		{
			var enemy = EntityManager.GetEntity("Enemy");
			if (enemy == null) return;

			if (evt.Current == SubPhase.EnemyStart)
			{
				ConvertGuardsToAggression(enemy);
			}
			else if (evt.Current == SubPhase.PreBlock)
			{
				TryGuardConversion(enemy);
			}
		}

		private void ConvertGuardsToAggression(Entity enemy)
		{
			var gq = enemy.GetComponent<GuardQueue>();
			if (gq == null || gq.Queue.Count == 0) return;

			int count = gq.Queue.Count;
			gq.Queue.Clear();

			EventQueueBridge.EnqueueTriggerAction("GuardManagementSystem.ConvertGuardsToAggression", () =>
			{
				EventManager.Publish(new ApplyPassiveEvent
				{
					Target = enemy,
					Type = AppliedPassiveType.Aggression,
					Delta = count
				});
			}, Duration);
		}

		private void TryGuardConversion(Entity enemy)
		{
			var ap = enemy.GetComponent<AppliedPassives>();
			if (ap == null || !ap.Passives.ContainsKey(AppliedPassiveType.Sentinel)) return;

			var intent = enemy.GetComponent<AttackIntent>();
			if (intent == null || intent.Planned == null || intent.Planned.Count == 0) return;

			// Process the first planned attack (the one being revealed at PreBlock)
			var planned = intent.Planned[0];
			var attackDef = planned.AttackDefinition;
			if (attackDef == null || attackDef.Damage <= 0) return;

			int damage = attackDef.Damage;
			int conversionAmount = RollGuardConversion(damage);
			if (conversionAmount <= 0) return;

			if (conversionAmount >= damage)
			{
				// Full conversion: skip this attack entirely
				intent.Planned.RemoveAt(0);

				EventQueueBridge.EnqueueTriggerAction("GuardManagementSystem.FullConversion", () =>
				{
					EventManager.Publish(new PassiveTriggered { Owner = enemy, Type = AppliedPassiveType.Sentinel });
					EventManager.Publish(new AddGuardEvent { Enemy = enemy, Value = damage });

					EventQueue.Clear();
					if (intent.Planned.Count == 0)
					{
						// No attacks remain — short-circuit to EnemyEnd -> PlayerStart -> Action
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
					else
					{
						// Attacks remain — re-trigger PreBlock for the next attack
						EventQueue.EnqueueRule(new EventQueueBridge.QueuedPublish<ChangeBattlePhaseEvent>(
							"Rule.ChangePhase.PreBlock.GuardSkip",
							new ChangeBattlePhaseEvent { Current = SubPhase.PreBlock }
						));
					}
				}, Duration);
			}
			else
			{
				// Partial conversion: reduce attack damage, gain guard
				attackDef.Damage -= conversionAmount;

				EventQueueBridge.EnqueueTriggerAction("GuardManagementSystem.PartialConversion", () =>
				{
					EventManager.Publish(new PassiveTriggered { Owner = enemy, Type = AppliedPassiveType.Sentinel });
					EventManager.Publish(new AddGuardEvent { Enemy = enemy, Value = conversionAmount });
				}, Duration);
			}
		}

		private int RollGuardConversion(int damage)
		{
			if (damage <= 0) return 0;
			// 50% chance of 0; remaining 50% uniform across 1..damage
			int roll = Random.Shared.Next(0, damage * 2);
			if (roll < damage)
			{
				return 0; // 50% chance: no conversion
			}
			return roll - damage + 1; // maps [damage..damage*2-1] to [1..damage]
		}

		private void OnLoadScene(LoadSceneEvent e)
		{
			// Cleanup guard queues on scene transitions
			foreach (var entity in GetRelevantEntities().ToList())
			{
				var gq = entity.GetComponent<GuardQueue>();
				if (gq != null) gq.Queue.Clear();
			}
		}
	}
}
