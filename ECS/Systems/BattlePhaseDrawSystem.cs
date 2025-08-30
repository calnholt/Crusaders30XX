using System.Linq;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Events;
using Microsoft.Xna.Framework;

namespace Crusaders30XX.ECS.Systems
{
	/// <summary>
	/// Draws cards based on phase transitions:
	/// - On StartOfBattle entry: draw up to Intellect (respect MaxHandSize)
	/// - On Block entry from phases other than StartOfBattle and ProcessEnemyAttack: draw up to Intellect
	/// </summary>
	public class BattlePhaseDrawSystem : Core.System
	{
		private BattlePhase _lastPhase = BattlePhase.StartOfBattle;
		private bool _didStartDraw;

		public BattlePhaseDrawSystem(EntityManager entityManager) : base(entityManager)
		{
			var s = entityManager.GetEntitiesWithComponent<BattlePhaseState>().FirstOrDefault()?.GetComponent<BattlePhaseState>();
			if (s != null) _lastPhase = s.Phase;
		}

		protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
		{
			return System.Array.Empty<Entity>();
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime) { }

		public override void Update(GameTime gameTime)
		{
			base.Update(gameTime);
			var state = EntityManager.GetEntitiesWithComponent<BattlePhaseState>().FirstOrDefault()?.GetComponent<BattlePhaseState>();
			if (state == null) return;
			var current = state.Phase;

			if (current == BattlePhase.StartOfBattle)
			{
				if (!_didStartDraw)
				{
					DrawUpToIntellect();
					_didStartDraw = true;
				}
			}
			else
			{
				_didStartDraw = false;
			}

			if (current == BattlePhase.Block && _lastPhase != BattlePhase.Block)
			{
				if (_lastPhase != BattlePhase.ProcessEnemyAttack && _lastPhase != BattlePhase.StartOfBattle)
				{
					DrawUpToIntellect();
				}
			}

			_lastPhase = current;
		}

		private void DrawUpToIntellect()
		{
			var player = EntityManager.GetEntitiesWithComponent<Player>().FirstOrDefault();
			if (player == null) return;
			int intellect = player.GetComponent<Intellect>()?.Value ?? 0;
			if (intellect <= 0) return;
			int maxHandSize = player.GetComponent<MaxHandSize>()?.Value ?? 0;
			var deckEntity = EntityManager.GetEntitiesWithComponent<Deck>().FirstOrDefault();
			var deck = deckEntity?.GetComponent<Deck>();
			if (deck == null) return;
			int spaceLeft = System.Math.Max(0, maxHandSize - deck.Hand.Count);
			int toDraw = System.Math.Min(spaceLeft, intellect);
			if (toDraw > 0)
			{
				EventManager.Publish(new RequestDrawCardsEvent { Count = toDraw });
			}
		}
	}
}


