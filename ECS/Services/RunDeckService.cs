using System;
using System.Collections.Generic;
using System.Linq;
using Crusaders30XX.ECS.Components;
using Crusaders30XX.ECS.Core;
using Crusaders30XX.ECS.Data.Loadouts;
using Crusaders30XX.ECS.Data.Save;
using Crusaders30XX.ECS.Factories;
using Crusaders30XX.ECS.Objects.Cards;

namespace Crusaders30XX.ECS.Services
{
	public static class RunDeckService
	{
		public const string PrimaryLoadoutId = "loadout_1";
		private const string DeckEntityName = "Deck";

		public static LoadoutDefinition GetLoadoutForRun()
		{
			return SaveCache.GetLoadout(PrimaryLoadoutId);
		}

		public static Entity EnsureRunDeck(EntityManager entityManager)
		{
			var deckEntity = GetOrCreateRunDeckEntity(entityManager);
			var deck = deckEntity.GetComponent<Deck>();
			if (deck == null) return deckEntity;

			var loadout = GetLoadoutForRun();
			var desiredKeys = BuildDesiredKeys(loadout);
			var existingByKey = entityManager
				.GetEntitiesWithComponent<RunDeckCard>()
				.Where(e => e.IsActive)
				.ToDictionary(e => e.GetComponent<RunDeckCard>().CardKey, e => e, StringComparer.OrdinalIgnoreCase);

			// Legacy loadouts may still list weapon keys; purge stale weapon entities so they never enter the draw pile.
			foreach (var kv in existingByKey.ToList())
			{
				if (!IsWeaponCardKey(kv.Key)) continue;
				RemoveCardFromDeckLists(deck, kv.Value);
				deck.Cards.Remove(kv.Value);
				entityManager.DestroyEntity(kv.Value.Id);
				existingByKey.Remove(kv.Key);
			}

			foreach (var key in desiredKeys)
			{
				if (existingByKey.ContainsKey(key)) continue;
				var created = CreateRunDeckCard(entityManager, key);
				if (created != null)
				{
					existingByKey[key] = created;
				}
			}

			var desiredSet = new HashSet<string>(desiredKeys, StringComparer.OrdinalIgnoreCase);
			foreach (var kv in existingByKey.ToList())
			{
				if (desiredSet.Contains(kv.Key)) continue;
				RemoveCardFromDeckLists(deck, kv.Value);
				deck.Cards.Remove(kv.Value);
				entityManager.DestroyEntity(kv.Value.Id);
			}

			deck.Cards.Clear();
			foreach (var key in desiredKeys)
			{
				if (existingByKey.TryGetValue(key, out var card) && card != null && card.IsActive)
				{
					deck.Cards.Add(card);
				}
			}

			RunScopedStateService.HydrateRunCardRestrictions(entityManager);

			return deckEntity;
		}

		public static void AddCardFromKey(EntityManager entityManager, string cardKey)
		{
			if (string.IsNullOrWhiteSpace(cardKey)) return;
			if (IsWeaponCardKey(cardKey)) return;
			var deckEntity = EnsureRunDeck(entityManager);
			var deck = deckEntity.GetComponent<Deck>();
			if (deck == null) return;

			var existing = entityManager
				.GetEntitiesWithComponent<RunDeckCard>()
				.FirstOrDefault(e => e.IsActive &&
					string.Equals(e.GetComponent<RunDeckCard>()?.CardKey, cardKey, StringComparison.OrdinalIgnoreCase));
			if (existing != null)
			{
				if (!deck.Cards.Contains(existing))
				{
					deck.Cards.Add(existing);
				}
				return;
			}

			var created = CreateRunDeckCard(entityManager, cardKey);
			if (created != null && !deck.Cards.Contains(created))
			{
				deck.Cards.Add(created);
			}
		}

		public static void RemoveCardByKey(EntityManager entityManager, string cardKey)
		{
			if (string.IsNullOrWhiteSpace(cardKey)) return;
			var card = entityManager
				.GetEntitiesWithComponent<RunDeckCard>()
				.FirstOrDefault(e => e.IsActive &&
					string.Equals(e.GetComponent<RunDeckCard>()?.CardKey, cardKey, StringComparison.OrdinalIgnoreCase));
			if (card == null) return;

			var deckEntity = GetRunDeckEntity(entityManager);
			var deck = deckEntity?.GetComponent<Deck>();
			if (deck != null)
			{
				RemoveCardFromDeckLists(deck, card);
				deck.Cards.Remove(card);
			}

			entityManager.DestroyEntity(card.Id);
		}

		public static void ExhaustRunCard(EntityManager entityManager, Entity card)
		{
			if (card == null) return;
			var runDeck = card.GetComponent<RunDeckCard>();
			if (runDeck == null || string.IsNullOrWhiteSpace(runDeck.CardKey))
			{
				DestroyNonRunCard(entityManager, card);
				return;
			}

			var cardKey = runDeck.CardKey;
			var deckEntity = GetRunDeckEntity(entityManager);
			var deck = deckEntity?.GetComponent<Deck>();
			if (deck != null)
			{
				RemoveCardFromDeckLists(deck, card);
				deck.Cards.Remove(card);
			}

			SaveCache.RemoveCardFromLoadout(PrimaryLoadoutId, cardKey, publishChange: false);
			entityManager.DestroyEntity(card.Id);
		}

		public static void DestroyRunDeck(EntityManager entityManager)
		{
			RunScopedStateService.ClearRunCardRestrictionComponents(entityManager);
			foreach (var card in entityManager.GetEntitiesWithComponent<RunDeckCard>().ToList())
			{
				if (card != null && card.IsActive)
				{
					entityManager.DestroyEntity(card.Id);
				}
			}

			var deckEntity = GetRunDeckEntity(entityManager);
			if (deckEntity != null)
			{
				entityManager.DestroyEntity(deckEntity.Id);
			}
		}

		public static Entity GetRunDeckEntity(EntityManager entityManager)
		{
			var byName = entityManager.GetEntity(DeckEntityName);
			if (byName != null && byName.HasComponent<DontDestroyOnLoad>() && byName.HasComponent<Deck>())
			{
				return byName;
			}

			return entityManager
				.GetEntitiesWithComponent<Deck>()
				.FirstOrDefault(e => e.IsActive && e.HasComponent<DontDestroyOnLoad>());
		}

		private static Entity GetOrCreateRunDeckEntity(EntityManager entityManager)
		{
			var existing = GetRunDeckEntity(entityManager);
			if (existing != null) return existing;

			var entity = entityManager.CreateEntity(DeckEntityName);
			entityManager.AddComponent(entity, new Deck());
			entityManager.AddComponent(entity, new DontDestroyOnLoad());
			return entity;
		}

		private static Entity CreateRunDeckCard(EntityManager entityManager, string cardKey)
		{
			if (!TryParseCardKey(cardKey, out var cardId, out var color)) return null;
			var card = CardFactory.Create(cardId);
			if (card == null || card.IsWeapon) return null;

			int index = StableIndexForKey(cardKey);
			var entity = EntityFactory.CreateCardFromDefinition(
				entityManager,
				cardId,
				color,
				allowWeapons: false,
				index: index,
				cardKey: cardKey,
				persistForRun: true);
			if (entity != null && SaveCache.IsStarterCardKey(cardKey))
			{
				var cardData = entity.GetComponent<CardData>();
				if (cardData?.Card != null)
				{
					cardData.Card.IsStarter = true;
				}
			}
			return entity;
		}

		private static List<string> BuildDesiredKeys(LoadoutDefinition loadout)
		{
			var keys = new List<string>();
			if (loadout?.cardIds == null) return keys;
			foreach (var entry in loadout.cardIds)
			{
				if (string.IsNullOrWhiteSpace(entry)) continue;
				var key = entry.Trim();
				if (IsWeaponCardKey(key)) continue;
				keys.Add(key);
			}
			return keys;
		}

		private static bool IsWeaponCardKey(string cardKey)
		{
			if (!TryParseCardKey(cardKey, out var cardId, out _)) return false;
			var card = CardFactory.Create(cardId);
			return card?.IsWeapon == true;
		}

		public static bool TryParseCardKey(string cardKey, out string cardId, out CardData.CardColor color)
		{
			cardId = cardKey;
			color = CardData.CardColor.White;
			if (string.IsNullOrWhiteSpace(cardKey)) return false;

			int sep = cardKey.IndexOf('|');
			if (sep >= 0)
			{
				cardId = cardKey.Substring(0, sep);
				color = ParseColor(cardKey.Substring(sep + 1));
			}
			return !string.IsNullOrWhiteSpace(cardId) && CardFactory.Create(cardId) != null;
		}

		private static CardData.CardColor ParseColor(string color)
		{
			if (string.IsNullOrEmpty(color)) return CardData.CardColor.White;
			switch (color.Trim().ToLowerInvariant())
			{
				case "red": return CardData.CardColor.Red;
				case "black": return CardData.CardColor.Black;
				case "white":
				default: return CardData.CardColor.White;
			}
		}

		private static int StableIndexForKey(string cardKey)
		{
			unchecked
			{
				int hash = 17;
				foreach (char c in cardKey)
				{
					hash = hash * 31 + c;
				}
				return Math.Abs(hash % 100000);
			}
		}

		private static void RemoveCardFromDeckLists(Deck deck, Entity card)
		{
			if (deck == null || card == null) return;
			deck.DrawPile.Remove(card);
			deck.Hand.Remove(card);
			deck.DiscardPile.Remove(card);
			deck.ExhaustPile.Remove(card);
		}

		private static void DestroyNonRunCard(EntityManager entityManager, Entity card)
		{
			var deckEntity = entityManager.GetEntitiesWithComponent<Deck>().FirstOrDefault();
			var deck = deckEntity?.GetComponent<Deck>();
			if (deck != null)
			{
				RemoveCardFromDeckLists(deck, card);
				deck.Cards.Remove(card);
			}
			entityManager.DestroyEntity(card.Id);
		}
	}
}
