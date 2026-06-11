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
				"absolution",
				"smite",
				"fervor",
				"courageous",
				"reckoning",
				"increase_faith",
				"litany_of_wrath",
				"exaltation",
				"stab",
				"tempest",
				"hold_the_line",
			};
		}

		public static IReadOnlyList<string> GetSwordSingleCopyStarterCardPool()
		{
			return new[] { "fervor" };
		}

		public static IReadOnlyList<string> GetDaggerStarterCardPool()
		{
			return new[]
			{
				"crusade",
				"strike",
				"courageous",
				"reckoning",
				"sacrifice",
				"seize",
				"razor_storm",
				"rally_the_faithful",
				"sudden_thrust",
				"hidden_kunai",
				"zealous_vow",
			};
		}

		public static IReadOnlyList<string> GetDaggerSingleCopyStarterCardPool()
		{
			return new[] { "sacrifice" };
		}

		private static HashSet<string> BuildSingleCopySet(IReadOnlyList<string> singleCopyCardIds)
		{
			if (singleCopyCardIds == null || singleCopyCardIds.Count == 0)
			{
				return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			}

			return new HashSet<string>(
				singleCopyCardIds.Where(id => !string.IsNullOrWhiteSpace(id)),
				StringComparer.OrdinalIgnoreCase);
		}

		private static int GetMaxCopiesForStarterCard(string cardId, IReadOnlySet<string> singleCopyCardIds)
		{
			return singleCopyCardIds.Contains(cardId) ? 1 : DeckRules.MaxCopiesPerCardId;
		}

		public static List<string> Generate(
			IReadOnlyList<string> poolCardIds,
			int seed,
			IReadOnlyList<string> singleCopyCardIds = null)
		{
			var singleCopySet = BuildSingleCopySet(singleCopyCardIds);

			var result = TryGenerate(poolCardIds, singleCopySet, new Random(seed), relaxColorQuotas: false);
			if (result.Count >= DeckRules.StartingDeckSize) return result;

			result = TryGenerate(poolCardIds, singleCopySet, new Random(seed + 1), relaxColorQuotas: false);
			if (result.Count >= DeckRules.StartingDeckSize) return result;

			result = TryGenerate(poolCardIds, singleCopySet, new Random(seed + 2), relaxColorQuotas: true);
			if (result.Count < DeckRules.StartingDeckSize)
			{
				Console.WriteLine($"[StartingDeckGenerator] Built {result.Count}/{DeckRules.StartingDeckSize} cards from pool size {poolCardIds?.Count ?? 0}.");
			}
			return result;
		}

		private static List<string> TryGenerate(
			IReadOnlyList<string> poolCardIds,
			IReadOnlySet<string> singleCopyCardIds,
			Random rng,
			bool relaxColorQuotas)
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

			ReserveGuaranteedSingleCopyCards(
				distinctPool,
				singleCopyCardIds,
				rng,
				relaxColorQuotas,
				ref redLeft,
				ref whiteLeft,
				ref blackLeft,
				finalDeck,
				deckKeySet,
				cardIdUsage);

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
				if (usage >= GetMaxCopiesForStarterCard(pair.Id, singleCopyCardIds)) continue;

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

		private static void ReserveGuaranteedSingleCopyCards(
			IReadOnlyList<string> distinctPool,
			IReadOnlySet<string> singleCopyCardIds,
			Random rng,
			bool relaxColorQuotas,
			ref int redLeft,
			ref int whiteLeft,
			ref int blackLeft,
			List<string> finalDeck,
			HashSet<string> deckKeySet,
			Dictionary<string, int> cardIdUsage)
		{
			if (singleCopyCardIds == null || singleCopyCardIds.Count == 0) return;

			var poolSet = new HashSet<string>(distinctPool, StringComparer.OrdinalIgnoreCase);
			var guaranteedIds = singleCopyCardIds
				.Where(id => poolSet.Contains(id))
				.OrderBy(_ => rng.Next())
				.ToList();

			foreach (var cardId in guaranteedIds)
			{
				if (finalDeck.Count >= DeckRules.StartingDeckSize) break;
				if (!CardFactory.GetAllCards().TryGetValue(cardId, out var card) || card == null) continue;
				if (!card.CanAddToLoadout || card.IsWeapon || card.IsToken) continue;

				cardIdUsage.TryGetValue(cardId, out int usage);
				if (usage >= 1) continue;

				var colors = new List<string> { "Red", "White", "Black" };
				colors = colors.OrderBy(_ => rng.Next()).ToList();

				string chosenColor = null;
				foreach (var color in colors)
				{
					if (!relaxColorQuotas)
					{
						if (color == "Red" && redLeft <= 0) continue;
						if (color == "White" && whiteLeft <= 0) continue;
						if (color == "Black" && blackLeft <= 0) continue;
					}

					chosenColor = color;
					break;
				}

				if (chosenColor == null) continue;

				string key = $"{card.CardId}|{chosenColor}";
				if (deckKeySet.Contains(key)) continue;

				finalDeck.Add(key);
				deckKeySet.Add(key);
				cardIdUsage[card.CardId] = usage + 1;

				if (chosenColor == "Red") redLeft--;
				else if (chosenColor == "White") whiteLeft--;
				else blackLeft--;
			}
		}
	}
}
