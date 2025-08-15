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

		public BattlePhaseSystem(EntityManager entityManager) : base(entityManager) { }

		public void Initialize()
		{
			Crusaders30XX.ECS.Core.EventManager.Subscribe<ChangeBattlePhaseEvent>(OnChangePhase);
		}

		protected override IEnumerable<Entity> GetRelevantEntities()
		{
			// Manage a singleton on any entity; create if missing
			return EntityManager.GetEntitiesWithComponent<BattlePhaseState>();
		}

		public override void Update(GameTime gameTime)
		{
			var state = GetOrCreatePhaseState();
			float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
			_phaseTimer += dt;

			switch (state.Phase)
			{
				case BattlePhase.StartOfBattle:
					if (AutoAdvanceStartSeconds > 0f && _phaseTimer >= AutoAdvanceStartSeconds)
					{
						TransitionTo(state, BattlePhase.Block);
					}
					break;
				case BattlePhase.Block:
					// Stay in Block until an external call transitions; placeholder no-op
					break;
				case BattlePhase.Action:
					// Action phase progresses via gameplay; placeholder no-op
					break;
			}

			base.Update(gameTime);
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime)
		{
			// This system manages a singleton component and does not iterate per-entity logic
		}

		private BattlePhaseState GetOrCreatePhaseState()
		{
			var e = EntityManager.GetEntitiesWithComponent<BattlePhaseState>().FirstOrDefault();
			if (e != null) return e.GetComponent<BattlePhaseState>();
			var world = EntityManager.CreateEntity("BattlePhaseState");
			var s = new BattlePhaseState { Phase = BattlePhase.StartOfBattle };
			EntityManager.AddComponent(world, s);
			_phaseTimer = 0f;
			return s;
		}

		private void TransitionTo(BattlePhaseState state, BattlePhase next)
		{
			state.Phase = next;
			_phaseTimer = 0f;
		}

		private void OnChangePhase(ChangeBattlePhaseEvent evt)
		{
			var s = GetOrCreatePhaseState();
			TransitionTo(s, evt.Next);
		}

		// Debug helpers to force transitions
		[DebugAction("To Start Of Battle")]
		public void Debug_ToStart()
		{
			EventManager.Publish(new ChangeBattlePhaseEvent { Next = BattlePhase.StartOfBattle });
		}

		[DebugAction("To Block Phase")]
		public void Debug_ToBlock()
		{
			EventManager.Publish(new ChangeBattlePhaseEvent { Next = BattlePhase.Block });
		}

		[DebugAction("To Action Phase")]
		public void Debug_ToAction()
		{
			EventManager.Publish(new ChangeBattlePhaseEvent { Next = BattlePhase.Action });
		}
	}
}


