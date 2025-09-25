using System.Linq;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Data.Cards;
using Crusaders30XX.ECS.Events;
using Microsoft.Xna.Framework;

namespace Crusaders30XX.ECS.Systems
{
	/// <summary>
	/// Draws cards based on phase transitions:
	/// - On StartOfBattle entry: draw up to Intellect (respect MaxHandSize)
	/// - On Block entry from phases other than StartOfBattle and ProcessEnemyAttack: draw up to Intellect
	/// </summary>
	public class DrawHandSystem : Core.System
	{

		public DrawHandSystem(EntityManager entityManager) : base(entityManager)
		{
			var s = entityManager.GetEntitiesWithComponent<PhaseState>().FirstOrDefault()?.GetComponent<PhaseState>();
			EventManager.Subscribe<ChangeBattlePhaseEvent>(_ => {
				if (_.Current == SubPhase.EnemyStart) {
					DrawUpToIntellect();
				}
			});
		}

		protected override System.Collections.Generic.IEnumerable<Entity> GetRelevantEntities()
		{
			return System.Array.Empty<Entity>();
		}

		protected override void UpdateEntity(Entity entity, GameTime gameTime)
		{
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
			// Do not count weapon cards toward the hand size limit
			int weaponCount = 0;
            foreach (var e in deck.Hand)
			{
				var cd = e.GetComponent<CardData>();
				if (cd == null) continue;
                string id = cd.CardId ?? string.Empty;
                if (string.IsNullOrEmpty(id)) continue;
                if (CardDefinitionCache.TryGet(id, out var def))
				{
					if (def.isWeapon) weaponCount++;
				}
			}
			int effectiveHandCount = System.Math.Max(0, deck.Hand.Count - weaponCount);
			int spaceLeft = System.Math.Max(0, maxHandSize - effectiveHandCount);
			int toDraw = System.Math.Min(spaceLeft, intellect);
			if (toDraw > 0)
			{
				EventManager.Publish(new RequestDrawCardsEvent { Count = toDraw });
			}
			if (deck.DrawPile.Count == 0)
			{
				EventManager.Publish(new PlayerDied { Player = EntityManager.GetEntity("Player") });
			}
		}
	}
}


