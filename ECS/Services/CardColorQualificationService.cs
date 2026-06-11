using System;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Objects.EnemyAttacks;

namespace Crusaders30XX.ECS.Services
{
	public static class CardColorQualificationService
	{
		public static CardData.CardColor? GetQualifiedColor(Entity card)
		{
			if (card == null || card.HasComponent<Colorless>()) return null;

			var color = card.GetComponent<CardData>()?.Color;
			return color is CardData.CardColor.Red or CardData.CardColor.White or CardData.CardColor.Black
				? color
				: null;
		}

		public static bool QualifiesAs(Entity card, CardData.CardColor color)
		{
			return GetQualifiedColor(card) == color;
		}

		public static bool IsEligibleForCost(Entity card, string cost)
		{
			if (card?.GetComponent<CardData>() == null || string.IsNullOrWhiteSpace(cost)) return false;
			if (string.Equals(cost, "Any", StringComparison.OrdinalIgnoreCase))
			{
				return card.GetComponent<CardData>().Color != CardData.CardColor.Yellow;
			}

			if (!Enum.TryParse<CardData.CardColor>(cost, true, out var requiredColor)) return false;
			return QualifiesAs(card, requiredColor);
		}

		public static bool MeetsBlockingRestriction(Entity card, BlockingRestrictionType restriction)
		{
			return restriction switch
			{
				BlockingRestrictionType.OnlyRed => QualifiesAs(card, CardData.CardColor.Red),
				BlockingRestrictionType.OnlyWhite => QualifiesAs(card, CardData.CardColor.White),
				BlockingRestrictionType.OnlyBlack => QualifiesAs(card, CardData.CardColor.Black),
				BlockingRestrictionType.NotRed => !QualifiesAs(card, CardData.CardColor.Red),
				BlockingRestrictionType.NotWhite => !QualifiesAs(card, CardData.CardColor.White),
				BlockingRestrictionType.NotBlack => !QualifiesAs(card, CardData.CardColor.Black),
				_ => true,
			};
		}
	}
}
