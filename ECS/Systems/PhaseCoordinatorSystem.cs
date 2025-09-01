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
			EventManager.Subscribe<ProceedToNextPhase>(_ => OnProceed());
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

		private void OnProceed()
		{
			var ps = GetOrCreate();
			switch (ps.Main)
			{
				case MainPhase.StartBattle:
					ps.Main = MainPhase.EnemyTurn; 
          ps.Sub = SubPhase.EnemyStart; 
          ps.TurnNumber = 1;
					EventManager.Publish(new ChangeBattlePhaseEvent { Current = SubPhase.EnemyStart, Previous = SubPhase.None });
          EventManager.Publish(new ProceedToNextPhase {  });
					break;
				case MainPhase.EnemyTurn:
					switch (ps.Sub)
					{
						case SubPhase.EnemyStart:
							ps.Sub = SubPhase.Block;
							EventManager.Publish(new ChangeBattlePhaseEvent { Current = SubPhase.Block, Previous = SubPhase.None });
							break;
						case SubPhase.Block:
							ps.Sub = SubPhase.EnemyAttack;
							EventManager.Publish(new ChangeBattlePhaseEvent { Current = SubPhase.EnemyAttack, Previous = SubPhase.Block });
							break;
						case SubPhase.EnemyAttack:
              // If any planned attacks remain on any enemy, go back to Block for re-assignment; otherwise go to Action
              bool hasNext = EntityManager.GetEntitiesWithComponent<AttackIntent>()
                  .Any(en =>
                  {
                      var i = en.GetComponent<AttackIntent>();
                      return i != null && i.Planned != null && i.Planned.Count > 0;
                  });
              Console.WriteLine($"[PhaseCoordinatorSystem] does enemy have another planned attack: {hasNext}");
              if (hasNext)
              {
                ps.Sub = SubPhase.Block;
                EventManager.Publish(new ChangeBattlePhaseEvent { Current = SubPhase.Block, Previous = SubPhase.EnemyAttack });
              }
              else
              {
                ps.Sub = SubPhase.EnemyEnd;
                EventManager.Publish(new ChangeBattlePhaseEvent { Current = SubPhase.EnemyEnd, Previous = SubPhase.EnemyAttack });
              }
							break;
						case SubPhase.EnemyEnd:
							ps.Sub = SubPhase.PlayerStart; ps.Main = MainPhase.PlayerAction;
							EventManager.Publish(new ChangeBattlePhaseEvent { Current = SubPhase.PlayerStart, Previous = SubPhase.EnemyEnd });
							break;
					}
					break;
				case MainPhase.PlayerAction:
					switch (ps.Sub)
					{
						case SubPhase.PlayerStart:
							ps.Sub = SubPhase.Action;
							EventManager.Publish(new ChangeBattlePhaseEvent { Current = SubPhase.Action, Previous = SubPhase.PlayerStart });
							break;
						case SubPhase.Action:
							ps.Sub = SubPhase.PlayerEnd;
							EventManager.Publish(new ChangeBattlePhaseEvent { Current = SubPhase.PlayerEnd, Previous = SubPhase.Action });
							break;
						case SubPhase.PlayerEnd:
							ps.Main = MainPhase.EnemyTurn; ps.Sub = SubPhase.EnemyStart; ps.TurnNumber++;
							EventManager.Publish(new ChangeBattlePhaseEvent { Current = SubPhase.EnemyStart, Previous = SubPhase.PlayerEnd });
							break;
					}
					break;
			}
      Console.WriteLine($"[PhaseCoordinatorSystem]: update phase - {ps.Main}: {ps.Sub}");
		}
	}
}


