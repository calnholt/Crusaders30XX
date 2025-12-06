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
		private int _lastTurn = -1;
		private bool _firstBlockOfTurn = true;

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
			if (e == null) return null;
			return e.GetComponent<PhaseState>();
		}

		private void OnChangeBattlePhaseEvent(ChangeBattlePhaseEvent evt)
		{
			var ps = GetOrCreate();
			if (ps == null) return;
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

			// Handle Block phase: trigger attack display for subsequent attacks
			if (evt.Current == SubPhase.Block)
			{
				// Reset first block flag when turn changes
				if (ps.TurnNumber != _lastTurn)
				{
					_lastTurn = ps.TurnNumber;
					_firstBlockOfTurn = true;
				}

				if (_firstBlockOfTurn)
				{
					// First attack of turn - let BattlePhaseDisplaySystem show the animation
					// PhaseChangeEventSystem will trigger the attack display after animation completes
					_firstBlockOfTurn = false;
				}
				else
				{
					// Subsequent attack - no animation, trigger attack display immediately
					var enemy = EntityManager.GetEntitiesWithComponent<AttackIntent>().FirstOrDefault();
					var intent = enemy?.GetComponent<AttackIntent>();
					if (intent != null && intent.Planned.Count > 0)
					{
						var contextId = intent.Planned[0].ContextId;
						EventManager.Publish(new TriggerEnemyAttackDisplayEvent { ContextId = contextId });
					}
				}
			}
		}

	}
}


