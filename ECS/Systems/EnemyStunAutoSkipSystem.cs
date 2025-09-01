using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Events;
using Microsoft.Xna.Framework;

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
			bool skippedAny = false;
			for (int i = 0; i < toSkip; i++)
			{
				var ctx = intent.Planned[0].ContextId;
				// Show overlay and remove the attack
				EventManager.Publish(new ShowStunnedOverlay { ContextId = ctx });
				intent.Planned.RemoveAt(0);
				stun.Stacks -= 1;
				skippedAny = true;
				if (stun.Stacks <= 0 || intent.Planned.Count == 0) break;
			}
			// If we skipped, let coordinator pick the next phase
			if (skippedAny)
			{
				EventManager.Publish(new ProceedToNextPhase());
			}
		}
	}
}


