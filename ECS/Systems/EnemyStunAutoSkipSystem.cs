using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Events;
using Microsoft.Xna.Framework;
using System;

namespace Crusaders30XX.ECS.Systems
{
	/// <summary>
	/// During Block phase, automatically skip as many leading planned attacks as there are stun stacks.
	/// Emits ShowStunnedOverlay for each skipped attack.
	/// </summary>
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
			bool duringAttack = enemyTurn && phase.Sub == SubPhase.EnemyAttack;
			var intent = enemy.GetComponent<AttackIntent>();
			if (intent == null || intent.Planned == null || intent.Planned.Count == 0) return;
			int startIdx = duringAttack ? System.Math.Min(1, intent.Planned.Count - 1) : 0;
			int toApply = System.Math.Max(0, e.Delta);
			for (int k = 0; k < toApply; k++)
			{
				int idx = -1;
				for (int i = startIdx; i < intent.Planned.Count; i++)
				{
					if (!intent.Planned[i].IsStunned)
					{ idx = i; break; }
				}
				if (idx < 0) break;
				intent.Planned[idx].IsStunned = true;
				System.Console.WriteLine($"[EnemyStunAutoSkipSystem] Marked planned attack index {idx} as stunned");
				startIdx = idx + 1; // subsequent deltas move forward
			}
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime)
		{
			var phase = EntityManager.GetEntitiesWithComponent<PhaseState>().FirstOrDefault().GetComponent<PhaseState>();
			if (phase.Sub != SubPhase.Block) return;
			if (_blockGraceTimerSeconds > 0f) return;
			var intent = entity.GetComponent<AttackIntent>();
			if (intent == null || intent.Planned == null || intent.Planned.Count == 0) return;
			while (intent.Planned.Count > 0 && intent.Planned[0].IsStunned)
			{
				var ctx = intent.Planned[0].ContextId;
				EventManager.Publish(new ShowStunnedOverlay { ContextId = ctx });
				intent.Planned.RemoveAt(0);
			}
			if (HasNextAttack())
			{
				Console.WriteLine("[EnemyStunAutoSkipSystem] has another attack");
				EventManager.Publish(new ChangeBattlePhaseEvent { Current = SubPhase.Block, Previous = SubPhase.EnemyAttack });
			}
			else {
				Console.WriteLine("[EnemyStunAutoSkipSystem] does not have another attack");
				EventManager.Publish(new ChangeBattlePhaseEvent { Current = SubPhase.EnemyEnd, Previous = SubPhase.Block });
				EventManager.Publish(new ChangeBattlePhaseEvent { Current = SubPhase.PlayerStart, Previous = SubPhase.EnemyEnd });
				EventManager.Publish(new ChangeBattlePhaseEvent { Current = SubPhase.Action, Previous = SubPhase.PlayerStart });
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

	}
}


