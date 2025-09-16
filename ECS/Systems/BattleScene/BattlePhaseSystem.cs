using System;
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.Diagnostics;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Microsoft.Xna.Framework;
using Crusaders30XX.ECS.Events;

namespace Crusaders30XX.ECS.Systems
{
	/// <summary>
	/// Manages the world battle phase component and transitions between phases.
	/// Phases: StartOfBattle -> Block -> Action (loops Action until external trigger moves back).
	/// </summary>
	[DebugTab("Battle Phases")]
	public class BattlePhaseSystem : Core.System
	{
		// Optional timers to demonstrate progression; can be driven by gameplay later
		[DebugEditable(DisplayName = "Auto-Advance Start->Block (s)", Step = 0.1f, Min = 0f, Max = 30f)]
		public float AutoAdvanceStartSeconds { get; set; } = 2f;

		private float _phaseTimer;

		public BattlePhaseSystem(EntityManager entityManager) : base(entityManager) { 
			EventManager.Subscribe<ChangeBattlePhaseEvent>(OnChangePhase);
			Console.WriteLine("[BattlePhaseSystem] Subscribed to ChangeBattlePhaseEvent");
		}

		protected override IEnumerable<Entity> GetRelevantEntities()
		{
			// Manage a singleton on any entity; create if missing
			return EntityManager.GetEntitiesWithComponent<PhaseState>();
		}

		public override void Update(GameTime gameTime)
		{
			var phase = EntityManager.GetEntitiesWithComponent<PhaseState>().FirstOrDefault().GetComponent<PhaseState>();
			float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
			_phaseTimer += dt;

			if (phase.Main == MainPhase.StartBattle) {
					if (AutoAdvanceStartSeconds > 0f && _phaseTimer >= AutoAdvanceStartSeconds)
					{
						EventQueue.EnqueueRule(new EventQueueBridge.QueuedPublish<ChangeBattlePhaseEvent>(
							"Rule.ChangePhase.EnemyStart",
							new ChangeBattlePhaseEvent { Current = SubPhase.EnemyStart }
						));
						EventQueue.EnqueueRule(new EventQueueBridge.QueuedPublish<ChangeBattlePhaseEvent>(
							"Rule.ChangePhase.Block",
							new ChangeBattlePhaseEvent { Current = SubPhase.Block }
						));
					}
			}

			base.Update(gameTime);
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime)
		{
			// This system manages a singleton component and does not iterate per-entity logic
		}

		private void OnChangePhase(ChangeBattlePhaseEvent evt)
		{
			_phaseTimer = 0f;
		}

	}
}


