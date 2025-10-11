using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.Diagnostics;
using Microsoft.Xna.Framework;
using System;

namespace Crusaders30XX.ECS.Systems
{
	[DebugTab("HP Management")]
	public class HpManagementSystem : Core.System
	{
		public HpManagementSystem(EntityManager entityManager) : base(entityManager)
		{
			EventManager.Subscribe<ModifyHpRequestEvent>(OnModifyHpRequest);
			EventManager.Subscribe<SetHpEvent>(OnSetHp);
			EventManager.Subscribe<ApplyPassiveEvent>(OnApplyPassive);
		}

		protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
		{
			return EntityManager.GetEntitiesWithComponent<HP>();
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime) { }

		private void OnModifyHpRequest(ModifyHpRequestEvent e)
		{
			var target = ResolveTarget(e.Target);
			if (target == null) return;
			var hp = target.GetComponent<HP>();
			if (hp == null) return;
			int before = hp.Current;
			// TODO: iterate through applied passives and apply their effects
			int passiveDelta = AppliedPassivesService.GetPassiveDelta(e);
			int newDelta = e.Delta + passiveDelta;
			int nv = hp.Current + newDelta;
			EventManager.Publish(new ModifyHpEvent { Source = e.Source, Target = target, Delta = newDelta, DamageType = e.DamageType });
			hp.Current = System.Math.Max(0, System.Math.Min(hp.Max, nv));
			// If this is the player and we crossed to zero, publish PlayerDied once
			if (before > 0 && hp.Current == 0 && target.HasComponent<Player>())
			{
				EventManager.Publish(new PlayerDied { Player = target });
			}
			if (before > 0 && hp.Current == 0 && target.HasComponent<Enemy>())
			{
				EventQueue.Clear();
				TimerScheduler.Schedule(1f, () => {
					Console.WriteLine("[HpManagementSystem] Enemy died, execute transition");
					// is this the last enemy?
					var queuedEntity = EntityManager.GetEntity("QueuedEvents");
					var queued = queuedEntity.GetComponent<QueuedEvents>();
					if (queued.CurrentIndex == queued.Events.Count - 1)
					{
						EventManager.Publish(new ShowTransition { Scene = SceneId.WorldMap });
					}
					else
					{
						EventManager.Publish(new ShowTransition { Scene = SceneId.Battle });
					}
				});
			}
		}

		private void OnSetHp(SetHpEvent e)
		{
			var target = ResolveTarget(e.Target);
			if (target == null) return;
			var hp = target.GetComponent<HP>();
			if (hp == null) return;
			int before = hp.Current;
			hp.Current = System.Math.Max(0, System.Math.Min(hp.Max, e.Value));
			if (before > 0 && hp.Current == 0 && target.HasComponent<Player>())
			{
				EventManager.Publish(new PlayerDied { Player = target });
			}
		}

		private Entity ResolveTarget(Entity explicitTarget)
		{
			if (explicitTarget != null) return explicitTarget;
			// Default: first Player with HP
			var player = EntityManager.GetEntitiesWithComponent<Player>().FirstOrDefault();
			if (player != null && player.HasComponent<HP>()) return player;
			return EntityManager.GetEntitiesWithComponent<HP>().FirstOrDefault();
		}

		private void OnApplyPassive(ApplyPassiveEvent e)
		{
			switch (e.Type)
			{
				case AppliedPassiveType.Penance:
					var hp = e.Target.GetComponent<HP>();
					var atFullHp = hp.Current == hp.Max;
					if (e.Delta > 0)
					{
						hp.Max -= e.Delta;
						if (atFullHp)
						{
							hp.Current -= e.Delta;
						}
					}
					else
					{
						hp.Max = Math.Min(30, hp.Max + e.Delta);
					}
					break;
				default:
					return;
			}
		}

		// Debug action: Lose X HP
		[DebugActionInt("Lose HP", Step = 1, Min = 1, Max = 999, Default = 10)]
		public void Debug_LoseHp(int amount)
		{
			EventManager.Publish(new ModifyHpRequestEvent { Source = EntityManager.GetEntity("Player"), Target = EntityManager.GetEntity("Player"), Delta = -System.Math.Abs(amount) });
		}

		// Debug action: Heal X HP
		[DebugActionInt("Heal HP", Step = 1, Min = 1, Max = 999, Default = 10)]
		public void Debug_HealHp(int amount)
		{
			EventManager.Publish(new ModifyHpRequestEvent { Source = EntityManager.GetEntity("Player"), Target = EntityManager.GetEntity("Player"), Delta = System.Math.Abs(amount) });
		}
	}
}


