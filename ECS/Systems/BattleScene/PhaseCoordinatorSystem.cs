using System.Linq;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Events;
using Microsoft.Xna.Framework;
using System;

namespace Crusaders30XX.ECS.Systems
{
	/// <summary>
	/// Central coordinator that advances the new main/sub-phase model when ProceedToNextPhase is published.
	/// Minimal scaffolding for now: wires the sequence described by the user.
	/// </summary>
	public class PhaseCoordinatorSystem : Core.System
	{
		public PhaseCoordinatorSystem(EntityManager em) : base(em)
		{
			EventManager.Subscribe<ChangeBattlePhaseEvent>(OnChangeBattlePhaseEvent);
		}

		protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
		{
			return EntityManager.GetEntitiesWithComponent<PhaseState>();
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime) { }

		private PhaseState GetOrCreate()
		{
			var e = EntityManager.GetEntitiesWithComponent<PhaseState>().FirstOrDefault();
			return e.GetComponent<PhaseState>();
		}

		private void OnChangeBattlePhaseEvent(ChangeBattlePhaseEvent evt)
		{
			var ps = GetOrCreate();
      if (evt.Current == SubPhase.EnemyStart) {
        ps.TurnNumber++;
      }
      if (evt.Current == SubPhase.EnemyStart || evt.Current == SubPhase.PreBlock || evt.Current == SubPhase.Block || evt.Current == SubPhase.EnemyAttack || evt.Current == SubPhase.EnemyEnd) {
        ps.Main = MainPhase.EnemyTurn;
      }
      else if (evt.Current == SubPhase.StartBattle) {
        ps.Main = MainPhase.StartBattle;
      }
      else {
        ps.Main = MainPhase.PlayerTurn;
      }
      ps.Sub = evt.Current;
		}

	}
}


