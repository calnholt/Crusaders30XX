using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Events;
using Crusaders30XX.Diagnostics;
using Microsoft.Xna.Framework;
using System;
using System.Diagnostics.Tracing;
using Crusaders30XX.ECS.Services;

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
			EventManager.Subscribe<FullyHealEvent>(OnFullyHeal);
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
			if (e.DamageType == ModifyTypeEnum.Heal && newDelta < 0)
			{
				// Prevent heals from turning into damage after passive adjustments
				newDelta = 0;
			}
			if (e.DamageType == ModifyTypeEnum.Attack && newDelta > 0)
			{
				return;
			}
			var targetPassives = target.GetComponent<AppliedPassives>()?.Passives;
			if (targetPassives != null && newDelta < 0)
			{
				if (TryConsumeAegis(target, targetPassives, ref newDelta))
				{
					e.Delta = newDelta - passiveDelta;
				}
			}
			if (TryConsumeShield(target, targetPassives, newDelta))
			{
				return;
			}
			int nv = hp.Current + newDelta;
			EventManager.Publish(new ModifyHpEvent { Source = e.Source, Target = target, Delta = newDelta, DamageType = e.DamageType });
			hp.Current = Math.Max(0, Math.Min(hp.Max, nv));
			// If this is the player and we crossed to zero, publish PlayerDied once
			if (before > 0 && hp.Current == 0 && target.HasComponent<Player>())
			{
				EventManager.Publish(new PlayerDied { Player = target });
			}
			else if (before > 0 && hp.Current == 0 && target.HasComponent<Enemy>())
			{
				EventQueue.Clear();
					TimerScheduler.Schedule(1f, () => {
					Console.WriteLine("[HpManagementSystem] Enemy died, execute transition");
					// is this the last enemy?
					var queuedEntity = EntityManager.GetEntity("QueuedEvents");
					var queued = queuedEntity.GetComponent<QueuedEvents>();
						if (queued != null 
							&& queued.Events != null 
							&& queued.Events.Count > 0 
							&& queued.CurrentIndex >= 0 
							&& queued.CurrentIndex == queued.Events.Count - 1)
					{
						Console.WriteLine($"[HpManagementSystem] Attempting to save quest completion");
						var completion = QuestCompleteService.SaveIfCompletedHighest(EntityManager);
						string msg = completion.IsNewlyCompleted
							? $"Quest Complete!\nGold +{completion.RewardGold}"
							: "Quest Complete! (already completed - no reward)";
						EventManager.Publish(new ShowQuestRewardOverlay { Message = msg });
						EventManager.Publish(new ChangeMusicTrack { Track = MusicTrack.QuestComplete });
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
			hp.Current = Math.Max(0, Math.Min(hp.Max, e.Value));
			if (before > 0 && hp.Current == 0 && target.HasComponent<Player>())
			{
				EventManager.Publish(new PlayerDied { Player = target });
			}
		}

		private void OnFullyHeal(FullyHealEvent e)
		{
			var target = ResolveTarget(e.Target);
			if (target == null || !target.HasComponent<Player>()) return;
			var hp = target.GetComponent<HP>();
			if (hp == null) return;
			hp.Current = hp.Max; // fully heal
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
					if (hp == null) return;
					var atFullHp = hp.Current == hp.Max;
					if (e.Delta > 0)
					{
						hp.Max = Math.Max(1, hp.Max - e.Delta);
						if (atFullHp)
						{
							hp.Current -= e.Delta;
						}
					}
					else if (e.Delta < 0)
					{
						int amount = -e.Delta;
						hp.Max = Math.Min(25, hp.Max + amount);
					}
					hp.Current = Math.Max(0, Math.Min(hp.Current, hp.Max));
					break;
				default:
					return;
			}
		}

		// Debug action: Lose X HP
		[DebugActionInt("Lose HP", Step = 1, Min = 1, Max = 999, Default = 10)]
		public void Debug_LoseHp(int amount)
		{
			EventManager.Publish(new ModifyHpRequestEvent { Source = EntityManager.GetEntity("Player"), Target = EntityManager.GetEntity("Player"), Delta = -Math.Abs(amount) });
		}

		// Debug action: Heal X HP
		[DebugActionInt("Heal HP", Step = 1, Min = 1, Max = 999, Default = 10)]
		public void Debug_HealHp(int amount)
		{
			EventManager.Publish(new ModifyHpRequestEvent { Source = EntityManager.GetEntity("Player"), Target = EntityManager.GetEntity("Player"), Delta = Math.Abs(amount) });
		}

		private bool TryConsumeShield(Entity target, System.Collections.Generic.Dictionary<AppliedPassiveType, int> passives, int newDelta)
		{
			if (newDelta >= 0 || passives == null) return false;
			if (!passives.TryGetValue(AppliedPassiveType.Shield, out var shieldStacks) || shieldStacks <= 0) return false;
			EventManager.Publish(new RemovePassive { Owner = target, Type = AppliedPassiveType.Shield });
			return true;
		}

		private bool TryConsumeAegis(Entity target, System.Collections.Generic.Dictionary<AppliedPassiveType, int> passives, ref int newDelta)
		{
			if (newDelta >= 0 || passives == null) return false;
			if (!passives.TryGetValue(AppliedPassiveType.Aegis, out var aegisAmount) || aegisAmount <= 0) return false;
			int damageAmount = Math.Abs(newDelta);
			int useAegis = Math.Min(aegisAmount, damageAmount);
			if (useAegis <= 0) return false;
			EventManager.Publish(new UpdatePassive { Owner = target, Type = AppliedPassiveType.Aegis, Delta = -useAegis });
			newDelta += useAegis;
			return true;
		}
	}

}


