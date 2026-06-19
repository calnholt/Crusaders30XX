using System;
using System.Linq;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;

namespace Crusaders30XX.ECS.Services
{
	public static class PlayerHandColorService
	{
		public static CardData.CardColor? GetRandomCardColorInPlayerHand(EntityManager entityManager)
		{
			var deckEntity = entityManager.GetEntitiesWithComponent<Deck>().FirstOrDefault();
			var deck = deckEntity?.GetComponent<Deck>();
			var hand = deck?.Hand;
			if (hand == null) return null;
			var colors = hand
				.Select(CardColorQualificationService.GetQualifiedColor)
				.Where(color => color.HasValue)
				.Select(color => color.Value)
				.Distinct()
				.ToList();
			if (colors.Count == 0) return null;
			return colors[Random.Shared.Next(0, colors.Count)];
		}
	}
}
