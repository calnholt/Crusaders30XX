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
			if (e != null) return e.GetComponent<PhaseState>();
			var world = EntityManager.CreateEntity("PhaseState");
			var ps = new PhaseState { Main = MainPhase.StartBattle, Sub = SubPhase.None, TurnNumber = 1 };
			EntityManager.AddComponent(world, ps);
			return ps;
		}

		private void OnChangeBattlePhaseEvent(ChangeBattlePhaseEvent evt)
		{
			var ps = GetOrCreate();
      if (evt.Current == SubPhase.EnemyStart) {
        ps.TurnNumber++;
      }
      if (evt.Current == SubPhase.EnemyStart || evt.Current == SubPhase.Block || evt.Current == SubPhase.EnemyAttack || evt.Current == SubPhase.EnemyEnd) {
        ps.Main = MainPhase.EnemyTurn;
      }
      else if (evt.Current == SubPhase.None) {
        ps.Main = MainPhase.StartBattle;
      }
      else {
        ps.Main = MainPhase.PlayerTurn;
      }
      ps.Sub = evt.Current;
		}

	}
}


