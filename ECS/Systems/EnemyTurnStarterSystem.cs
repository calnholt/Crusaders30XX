using System.Linq;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Microsoft.Xna.Framework;
using Crusaders30XX.ECS.Events;

namespace Crusaders30XX.ECS.Systems
{
	/// <summary>
	/// Publishes StartEnemyTurn when entering Block phase and there are no current planned attacks.
	/// Decouples planning trigger from BattlePhaseSystem.
	/// </summary>
	public class EnemyTurnStarterSystem : Core.System
	{
		private BattlePhase _lastPhase = BattlePhase.StartOfBattle;

		public EnemyTurnStarterSystem(EntityManager entityManager) : base(entityManager) { }

		protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
		{
			return System.Array.Empty<Entity>();
		}

		public override void Update(GameTime gameTime)
		{
			base.Update(gameTime);
			var state = EntityManager.GetEntitiesWithComponent<BattlePhaseState>().FirstOrDefault()?.GetComponent<BattlePhaseState>();
			if (state == null) return;
			var current = state.Phase;
			if (current == BattlePhase.Block && _lastPhase != BattlePhase.Block)
			{
				System.Console.WriteLine("[EnemyTurnStarterSystem] Entered Block phase");
				bool hasPlanned = EntityManager.GetEntitiesWithComponent<AttackIntent>()
					.Any(e =>
					{
						var i = e.GetComponent<AttackIntent>();
						return i != null && i.Planned != null && i.Planned.Count > 0;
					});
				if (!hasPlanned)
				{
					System.Console.WriteLine("[EnemyTurnStarterSystem] Publishing StartEnemyTurn");
					EventManager.Publish(new StartEnemyTurn());
				}
			}
			_lastPhase = current;
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime) { }
	}
}



