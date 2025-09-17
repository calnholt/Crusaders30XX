using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Events;
using Microsoft.Xna.Framework;
using System;
using Crusaders30XX.Diagnostics;

namespace Crusaders30XX.ECS.Systems
{
	/// <summary>
	/// During Block phase, automatically skip as many leading planned attacks as there are stun stacks.
	/// Emits ShowStunnedOverlay for each skipped attack.
	/// </summary>
	[DebugTab("Stun System")] 
	public class EnemyStunAutoSkipSystem : Core.System
	{
		private float _blockGraceTimerSeconds = 0f;

		public EnemyStunAutoSkipSystem(EntityManager em) : base(em)
		{
			EventManager.Subscribe<ChangeBattlePhaseEvent>(e => { if (e.Current == SubPhase.Block) _blockGraceTimerSeconds = 0.01f; });
			EventManager.Subscribe<ApplyStun>(OnApplyStun);
		}

		protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
		{
			return EntityManager.GetEntitiesWithComponent<Enemy>();
		}

		public override void Update(GameTime gameTime)
		{
			if (_blockGraceTimerSeconds > 0f)
			{
				_blockGraceTimerSeconds = System.Math.Max(0f, _blockGraceTimerSeconds - (float)gameTime.ElapsedGameTime.TotalSeconds);
			}
			base.Update(gameTime);
		}

		private void OnApplyStun(ApplyStun e)
		{
			if (e == null || e.Delta == 0) return;
			var enemy = EntityManager.GetEntitiesWithComponent<Enemy>().FirstOrDefault();
			if (enemy == null) return;
			var phase = EntityManager.GetEntitiesWithComponent<PhaseState>().FirstOrDefault()?.GetComponent<PhaseState>();
			bool enemyTurn = phase != null && phase.Main == MainPhase.EnemyTurn;
			var intent = enemy.GetComponent<AttackIntent>();
			var next = enemy.GetComponent<NextTurnAttackIntent>();
			if ((intent == null || intent.Planned == null) && (next == null || next.Planned == null)) return;
			int toApply = System.Math.Max(0, e.Delta);

			int currentStartIdx = enemyTurn ? 1 : 0;
			int nextStartIdx = 0;

			bool TryMarkInList(System.Collections.Generic.List<PlannedAttack> list, ref int startIndex, string listName)
			{
				if (list == null || list.Count == 0) return false;
				for (int i = System.Math.Max(0, startIndex); i < list.Count; i++)
				{
					if (!list[i].IsStunned)
					{
						list[i].IsStunned = true;
						System.Console.WriteLine($"[EnemyStunAutoSkipSystem] Marked {listName} index {i} as stunned");
						startIndex = i + 1;
						return true;
					}
				}
				return false;
			}

			for (int k = 0; k < toApply; k++)
			{
				bool marked = false;
				// Enemy turn: never mark current (start at 1). Player turn: start at 0.
				if (intent != null)
				{
					marked = TryMarkInList(intent.Planned, ref currentStartIdx, "current");
				}
				if (!marked && next != null)
				{
					marked = TryMarkInList(next.Planned, ref nextStartIdx, "next-turn");
				}
				if (!marked) break;
			}
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime)
		{
			var phase = EntityManager.GetEntitiesWithComponent<PhaseState>().FirstOrDefault().GetComponent<PhaseState>();
			if (phase.Sub != SubPhase.Block) return;
			if (_blockGraceTimerSeconds > 0f) return;
			var intent = entity.GetComponent<AttackIntent>();
			if (intent == null || intent.Planned == null || intent.Planned.Count == 0) return;
			bool removedAny = false;
			while (intent.Planned.Count > 0 && intent.Planned[0].IsStunned)
			{
				var ctx = intent.Planned[0].ContextId;
				intent.Planned.RemoveAt(0);
				EventManager.Publish(new ShowStunnedOverlay { ContextId = ctx });
				removedAny = true;
			}
			// Only transition phases if we actually consumed stunned attacks this tick
			if (!removedAny) return;
			if (HasNextAttack())
			{
				Console.WriteLine("[EnemyStunAutoSkipSystem] has another attack");
				EventQueue.EnqueueRule(new EventQueueBridge.QueuedPublish<ChangeBattlePhaseEvent>(
					"Rule.ChangePhase.Block",
					new ChangeBattlePhaseEvent { Current = SubPhase.Block }
				));
			}
			else {
				Console.WriteLine("[EnemyStunAutoSkipSystem] does not have another attack");
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
		}

    private bool HasNextAttack() {
      return EntityManager.GetEntitiesWithComponent<AttackIntent>()
          .Any(en =>
          {
              var i = en.GetComponent<AttackIntent>();
              return i != null && i.Planned != null && i.Planned.Count > 0;
          });
    }

		[DebugAction("Apply Stun")]
		public void Debug_ApplyStun()
		{
			EventManager.Publish<ApplyStun>(new ApplyStun { Delta = 1 } );
		}

	}
}


