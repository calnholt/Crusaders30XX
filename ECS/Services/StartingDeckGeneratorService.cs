using System;
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.ECS.Data.Loadouts;
using Crusaders30XX.ECS.Factories;

namespace Crusaders30XX.ECS.Services
{
	public static class StartingDeckGeneratorService
	{
		public static readonly string[] DefaultStarterCardPool =
		{
			"anoint_the_sick",
			"smite",
			"fervor",
			"courageous",
			"reckoning",
			"absolution",
			"increase_faith",
			"litany_of_wrath",
			"exaltation",
			"seize",
			"shield_of_faith",
			"stab",
			"tempest",
			"razor_storm",
			"hold_the_line",
		};

		private static readonly HashSet<string> DefaultStarterCardPoolSet = new HashSet<string>(
			DefaultStarterCardPool,
			StringComparer.OrdinalIgnoreCase);

		public static bool IsInDefaultStarterPool(string cardId)
		{
			return !string.IsNullOrWhiteSpace(cardId) && DefaultStarterCardPoolSet.Contains(cardId);
		}

		public static IReadOnlyList<string> GetSwordStarterCardPool()
		{
			return new[]
			{
				"anoint_the_sick",
				"smite",
				"fervor",
				"courageous",
				"reckoning",
				"absolution",
				"increase_faith",
				"litany_of_wrath",
				"exaltation",
				"seize",
				"shield_of_faith",
				"stab",
				"tempest",
				"razor_storm",
				"hold_the_line",
			};
		}

		public static IReadOnlyList<string> GetDaggerStarterCardPool()
		{
			return new[]
			{
				"anoint_the_sick",
				"smite",
				"fervor",
				"courageous",
				"reckoning",
				"absolution",
				"increase_faith",
				"litany_of_wrath",
				"exaltation",
				"seize",
				"shield_of_faith",
				"stab",
				"tempest",
				"razor_storm",
				"hold_the_line",
			};
		}

		public static List<string> Generate(IReadOnlyList<string> poolCardIds, int seed)
		{
			var result = TryGenerate(poolCardIds, new Random(seed), relaxColorQuotas: false);
			if (result.Count >= DeckRules.StartingDeckSize) return result;

			result = TryGenerate(poolCardIds, new Random(seed + 1), relaxColorQuotas: false);
			if (result.Count >= DeckRules.StartingDeckSize) return result;

			result = TryGenerate(poolCardIds, new Random(seed + 2), relaxColorQuotas: true);
			if (result.Count < DeckRules.StartingDeckSize)
			{
				Console.WriteLine($"[StartingDeckGenerator] Built {result.Count}/{DeckRules.StartingDeckSize} cards from pool size {poolCardIds?.Count ?? 0}.");
			}
			return result;
		}

		private static List<string> TryGenerate(IReadOnlyList<string> poolCardIds, Random rng, bool relaxColorQuotas)
		{
			var finalDeck = new List<string>();
			var cardIdUsage = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
			var deckKeySet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

			int redLeft = 7, whiteLeft = 7, blackLeft = 7;
			int shortColorIndex = rng.Next(3);
			if (shortColorIndex == 0) redLeft = 6;
			else if (shortColorIndex == 1) whiteLeft = 6;
			else blackLeft = 6;

			var distinctPool = (poolCardIds ?? Array.Empty<string>())
				.Where(id => !string.IsNullOrWhiteSpace(id))
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.ToList();

			var allPairs = new List<(string Id, string Color)>();
			foreach (var cardId in distinctPool)
			{
				if (!CardFactory.GetAllCards().TryGetValue(cardId, out var card) || card == null) continue;
				if (!card.CanAddToLoadout || card.IsWeapon || card.IsToken) continue;

				allPairs.Add((card.CardId, "Red"));
				allPairs.Add((card.CardId, "White"));
				allPairs.Add((card.CardId, "Black"));
			}

			allPairs = allPairs.OrderBy(_ => rng.Next()).ToList();

			foreach (var pair in allPairs)
			{
				if (finalDeck.Count >= DeckRules.StartingDeckSize) break;

				string key = $"{pair.Id}|{pair.Color}";
				if (deckKeySet.Contains(key)) continue;

				cardIdUsage.TryGetValue(pair.Id, out int usage);
				if (usage >= DeckRules.MaxCopiesPerCardId) continue;

				if (!relaxColorQuotas)
				{
					if (pair.Color == "Red" && redLeft <= 0) continue;
					if (pair.Color == "White" && whiteLeft <= 0) continue;
					if (pair.Color == "Black" && blackLeft <= 0) continue;
				}

				finalDeck.Add(key);
				deckKeySet.Add(key);
				cardIdUsage[pair.Id] = usage + 1;

				if (pair.Color == "Red") redLeft--;
				else if (pair.Color == "White") whiteLeft--;
				else if (pair.Color == "Black") blackLeft--;
			}

			return finalDeck.OrderBy(_ => rng.Next()).ToList();
		}
	}
}
