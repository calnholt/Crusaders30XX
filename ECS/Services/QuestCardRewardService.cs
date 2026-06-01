using System;
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Data.Loadouts;
using Crusaders30XX.ECS.Data.Save;
using Crusaders30XX.ECS.Factories;
using Crusaders30XX.ECS.Objects.Cards;

namespace Crusaders30XX.ECS.Services
{
	public static class QuestCardRewardService
	{
		private static readonly CardData.CardColor[] RewardColors =
		{
			CardData.CardColor.Red,
			CardData.CardColor.White,
			CardData.CardColor.Black
		};

		public struct QuestCardRewardResult
		{
			public bool Granted;
			public string CardId;
			public CardData.CardColor Color;
			public string CardKey;
		}

		public static QuestCardRewardResult TryGrantRandomCard()
		{
			var result = new QuestCardRewardResult();
			var loadout = SaveCache.GetLoadout("loadout_1");
			var deckKeys = loadout?.cardIds ?? new List<string>();

			var eligible = BuildEligiblePairs(deckKeys);
			if (eligible.Count == 0) return result;

			var pick = eligible[Random.Shared.Next(eligible.Count)];
			string cardKey = $"{pick.cardId}|{ColorToString(pick.color)}";

			SaveCache.AddCardToLoadout("loadout_1", cardKey);

			result.Granted = true;
			result.CardId = pick.cardId;
			result.Color = pick.color;
			result.CardKey = cardKey;
			return result;
		}

		internal static IReadOnlyList<string> GetEligibleRewardCardIdsForTests(IReadOnlyList<string> deckKeys)
		{
			return BuildEligiblePairs(deckKeys)
				.Select(p => p.cardId)
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.ToList();
		}

		private static List<(string cardId, CardData.CardColor color)> BuildEligiblePairs(IReadOnlyList<string> deckKeys)
		{
			var eligible = new List<(string cardId, CardData.CardColor color)>();
			var deckKeySet = new HashSet<string>(deckKeys ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);

			foreach (var card in CardFactory.GetAllCards().Values)
			{
				if (!card.CanAddToLoadout || card.IsWeapon || card.IsToken) continue;

				string cardId = card.CardId;
				if (string.IsNullOrWhiteSpace(cardId)) continue;
				if (StartingDeckGeneratorService.IsInDefaultStarterPool(cardId)) continue;

				if (DeckRules.CountCardIdInDeck(deckKeys, cardId) >= DeckRules.MaxCopiesPerCardId) continue;

				foreach (var color in RewardColors)
				{
					string key = $"{cardId}|{ColorToString(color)}";
					if (deckKeySet.Contains(key)) continue;
					eligible.Add((cardId, color));
				}
			}

			return eligible;
		}

		private static string ColorToString(CardData.CardColor color)
		{
			return color switch
			{
				CardData.CardColor.Red => "Red",
				CardData.CardColor.Black => "Black",
				_ => "White"
			};
		}
	}
}
