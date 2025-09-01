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

		protected override void UpdateEntity(Entity entity, GameTime gameTime)
		{
			var phase = EntityManager.GetEntitiesWithComponent<PhaseState>().FirstOrDefault().GetComponent<PhaseState>();
			if (phase.Sub != SubPhase.Block) return;
			if (_blockGraceTimerSeconds > 0f) return;
			var stun = entity.GetComponent<Stun>();
			if (stun == null || stun.Stacks <= 0) return;
			var intent = entity.GetComponent<AttackIntent>();
			if (intent == null || intent.Planned == null || intent.Planned.Count == 0) return;
			int toSkip = System.Math.Min(stun.Stacks, intent.Planned.Count);
			for (int i = 0; i < toSkip; i++)
			{
				var ctx = intent.Planned[0].ContextId;
				// Show overlay and remove the attack
				EventManager.Publish(new ShowStunnedOverlay { ContextId = ctx });
				intent.Planned.RemoveAt(0);
				stun.Stacks -= 1;
				if (stun.Stacks <= 0 || intent.Planned.Count == 0) break;
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


